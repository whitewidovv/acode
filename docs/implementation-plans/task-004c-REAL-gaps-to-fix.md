# Task 004c - REAL Gaps to Fix (No Rationalizing)

**Date**: 2026-01-12
**Status**: Identified mandatory MUST requirements not implemented

---

## Methodology

1. Read spec FR requirements (all say "MUST")
2. Check actual code
3. Document what's missing (no excuses)
4. Implement everything marked MUST

---

## Gap #1: ProviderDescriptor Missing Required Properties

**Spec Requirements**:
- FR-020: ProviderDescriptor **MUST** include Priority property (int, for fallback ordering)
- FR-021: ProviderDescriptor **MUST** include Enabled property (bool)

**Current State**:
```csharp
// src/Acode.Application/Providers/ProviderDescriptor.cs
public sealed record ProviderDescriptor
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required ProviderType Type { get; init; }
    public required ProviderCapabilities Capabilities { get; init; }
    public required ProviderEndpoint Endpoint { get; init; }
    public ProviderConfig? Config { get; init; }
    public RetryPolicy? RetryPolicy { get; init; }
    public string? FallbackProviderId { get; init; }
    public Dictionary<string, string>? ModelMappings { get; init; }

    // ❌ MISSING: Priority property
    // ❌ MISSING: Enabled property
}
```

**What Needs to be Added**:
```csharp
/// <summary>
/// Gets or initializes the priority for fallback ordering (lower = preferred).
/// </summary>
/// <remarks>
/// FR-020: Priority property for fallback ordering.
/// Used when multiple fallback providers available.
/// </remarks>
public int Priority { get; init; } = 0;

/// <summary>
/// Gets or initializes whether this provider is enabled.
/// </summary>
/// <remarks>
/// FR-021: Enabled property to enable/disable providers.
/// Disabled providers are not available for selection.
/// </remarks>
public bool Enabled { get; init; } = true;
```

**Tests Needed**:
- Test Priority affects fallback ordering
- Test Enabled=false excludes from selection
- Update existing tests to set Priority/Enabled

**AC Affected**: AC-021, AC-022

---

## Gap #2: ProviderType Enum Wrong Values

**Spec Requirements**:
- FR-025: ProviderType **MUST** include Ollama value
- FR-026: ProviderType **MUST** include Vllm value
- FR-027: ProviderType **MUST** include Mock value
- FR-028: ProviderType **MUST** serialize to lowercase strings

**Current State**:
```csharp
// src/Acode.Application/Providers/ProviderType.cs
public enum ProviderType
{
    Local,      // ❌ NOT IN SPEC
    Remote,     // ❌ NOT IN SPEC
    Embedded    // ❌ NOT IN SPEC
}
```

**What Spec Says**:
```csharp
public enum ProviderType
{
    Ollama,  // ✅ REQUIRED
    Vllm,    // ✅ REQUIRED
    Mock     // ✅ REQUIRED
}
```

**Justification Check**: Is "Local/Remote/Embedded" actually better?
- **NO** - The spec is explicit about values for a reason
- Ollama and Vllm are the two providers in Epic 1
- Mock is needed for testing
- "Local/Remote" is a different classification (endpoint location vs provider type)
- If we wanted both classifications, we'd need BOTH properties

**What Must Change**:
Replace current enum with spec-compliant version:
```csharp
/// <summary>
/// Type of model provider.
/// </summary>
/// <remarks>
/// FR-024 to FR-028 from task-004c spec.
/// </remarks>
public enum ProviderType
{
    /// <summary>
    /// Ollama provider (local inference server).
    /// </summary>
    Ollama,

    /// <summary>
    /// vLLM provider (high-performance inference server).
    /// </summary>
    Vllm,

    /// <summary>
    /// Mock provider (for testing).
    /// </summary>
    Mock
}
```

**Impact Analysis**:
- All tests referencing ProviderType.Local/Remote/Embedded must be updated
- All code using ProviderType must be updated
- Integration tests using "ollama" string ID are fine
- Need to add serialization attribute for lowercase

**Tests to Update**:
- ProviderDescriptorTests.cs (references ProviderType)
- ProviderRegistryTests.cs (creates providers with types)
- All integration tests (check they use correct types)

**AC Affected**: AC-024, AC-025, AC-026, AC-027, AC-028

---

## Gap #3: ProviderCapabilities Missing Properties

**Spec Requirements**:
- FR-032: ProviderCapabilities **MUST** include SupportsToolCalls (bool)
- FR-033: ProviderCapabilities **MUST** include MaxContextTokens (int)
- FR-034: ProviderCapabilities **MUST** include MaxOutputTokens (int)
- FR-035: ProviderCapabilities **MUST** include SupportsJsonMode (bool)

**Current State**:
```csharp
// src/Acode.Application/Inference/ProviderCapabilities.cs
public sealed record ProviderCapabilities
{
    public bool SupportsStreaming { get; init; }        // ✅ OK
    public bool SupportsTools { get; init; }            // ⚠️ Spec says "SupportsToolCalls"
    public bool SupportsSystemMessages { get; init; }   // ✅ OK (bonus)
    public bool SupportsVision { get; init; }           // ✅ OK (bonus)
    public int? MaxContextLength { get; init; }         // ⚠️ Spec says "MaxContextTokens"
    public string[]? SupportedModels { get; init; }     // ✅ OK
    public string? DefaultModel { get; init; }          // ✅ OK

    // ❌ MISSING: MaxOutputTokens property
    // ❌ MISSING: SupportsJsonMode property
}
```

**What Needs to be Changed/Added**:

1. Rename or alias SupportsTools → SupportsToolCalls (or keep both for compat)
2. Rename or alias MaxContextLength → MaxContextTokens (or keep both)
3. Add MaxOutputTokens property
4. Add SupportsJsonMode property

**Options**:
- **Option A**: Rename existing properties (BREAKING CHANGE, requires test updates)
- **Option B**: Add new properties, keep old ones for compatibility
- **Recommendation**: Option A - we're pre-1.0, fix naming now

**Implementation**:
```csharp
public sealed record ProviderCapabilities
{
    // ... existing properties with correct names ...

    /// <summary>
    /// Gets a value indicating whether the provider supports tool/function calling.
    /// </summary>
    /// <remarks>FR-032: SupportsToolCalls property (bool).</remarks>
    public bool SupportsToolCalls { get; init; }  // RENAME from SupportsTools

    /// <summary>
    /// Gets the maximum context length in tokens.
    /// </summary>
    /// <remarks>FR-033: MaxContextTokens property.</remarks>
    public int MaxContextTokens { get; init; }  // RENAME from MaxContextLength

    /// <summary>
    /// Gets the maximum output length in tokens.
    /// </summary>
    /// <remarks>FR-034: MaxOutputTokens property (int).</remarks>
    public int MaxOutputTokens { get; init; }  // NEW

    /// <summary>
    /// Gets a value indicating whether the provider supports JSON mode output.
    /// </summary>
    /// <remarks>FR-035: SupportsJsonMode property (bool).</remarks>
    public bool SupportsJsonMode { get; init; }  // NEW
}
```

**AC Affected**: AC-032, AC-033, AC-034, AC-035

---

## Gap #4: ProviderCapabilities Missing Required Methods

**Spec Requirements**:
- FR-036: ProviderCapabilities **MUST** provide Supports(CapabilityRequirement) method
- FR-037: ProviderCapabilities **MUST** provide Merge(ProviderCapabilities) method

**Current State**:
```csharp
// src/Acode.Application/Inference/ProviderCapabilities.cs
public sealed record ProviderCapabilities
{
    // Properties only, no methods
    // ❌ MISSING: Supports() method
    // ❌ MISSING: Merge() method
}
```

**What Needs to be Added**:

First, define CapabilityRequirement:
```csharp
/// <summary>
/// Describes capability requirements for a request.
/// </summary>
public sealed record CapabilityRequirement
{
    public bool RequiresStreaming { get; init; }
    public bool RequiresToolCalls { get; init; }
    public bool RequiresJsonMode { get; init; }
    public int? MinContextTokens { get; init; }
    public int? MinOutputTokens { get; init; }
    public string? RequiredModel { get; init; }
}
```

Then add methods to ProviderCapabilities:
```csharp
/// <summary>
/// Checks if this provider supports the given capability requirements.
/// </summary>
/// <param name="requirement">Capability requirements to check.</param>
/// <returns>True if all requirements are met, false otherwise.</returns>
/// <remarks>FR-036: Supports method for capability matching.</remarks>
public bool Supports(CapabilityRequirement requirement)
{
    ArgumentNullException.ThrowIfNull(requirement);

    if (requirement.RequiresStreaming && !this.SupportsStreaming)
    {
        return false;
    }

    if (requirement.RequiresToolCalls && !this.SupportsToolCalls)
    {
        return false;
    }

    if (requirement.RequiresJsonMode && !this.SupportsJsonMode)
    {
        return false;
    }

    if (requirement.MinContextTokens.HasValue &&
        this.MaxContextTokens < requirement.MinContextTokens.Value)
    {
        return false;
    }

    if (requirement.MinOutputTokens.HasValue &&
        this.MaxOutputTokens < requirement.MinOutputTokens.Value)
    {
        return false;
    }

    if (requirement.RequiredModel != null &&
        this.SupportedModels != null &&
        !this.SupportedModels.Contains(requirement.RequiredModel))
    {
        return false;
    }

    return true;
}

/// <summary>
/// Merges this capabilities with another, taking the most capable values.
/// </summary>
/// <param name="other">Other capabilities to merge with.</param>
/// <returns>New capabilities with merged values.</returns>
/// <remarks>FR-037: Merge method for capability combination.</remarks>
public ProviderCapabilities Merge(ProviderCapabilities other)
{
    ArgumentNullException.ThrowIfNull(other);

    return new ProviderCapabilities(
        supportsStreaming: this.SupportsStreaming || other.SupportsStreaming,
        supportsTools: this.SupportsToolCalls || other.SupportsToolCalls,
        supportsJsonMode: this.SupportsJsonMode || other.SupportsJsonMode,
        maxContextTokens: Math.Max(this.MaxContextTokens, other.MaxContextTokens),
        maxOutputTokens: Math.Max(this.MaxOutputTokens, other.MaxOutputTokens),
        supportedModels: MergeSupportedModels(this.SupportedModels, other.SupportedModels),
        defaultModel: this.DefaultModel ?? other.DefaultModel);
}

private static string[]? MergeSupportedModels(string[]? a, string[]? b)
{
    if (a == null) return b;
    if (b == null) return a;
    return a.Union(b).ToArray();
}
```

**Tests Required** (from Testing Requirements section):
- Should_Check_Supports() - test Supports() method
- Should_Merge_Capabilities() - test Merge() method

**AC Affected**: AC-036, AC-037

---

## Gap #5: Missing Tests for Spec-Required Methods

**Spec Says** (lines 828-831):
```
├── ProviderCapabilitiesTests.cs
│   ├── Should_Check_Supports()
│   ├── Should_Merge_Capabilities()
│   └── Should_Be_Immutable()
```

**Current Tests**:
- ✅ Has 11 tests for properties
- ❌ Missing Should_Check_Supports()
- ❌ Missing Should_Merge_Capabilities()

**Tests to Add**:
```csharp
[Fact]
public void Should_Check_Supports()
{
    // Arrange
    var capabilities = new ProviderCapabilities(
        supportsStreaming: true,
        supportsTools: true,
        supportsJsonMode: false,
        maxContextTokens: 8192,
        maxOutputTokens: 2048);

    // Act & Assert - Matching requirements
    var req1 = new CapabilityRequirement { RequiresStreaming = true };
    capabilities.Supports(req1).Should().BeTrue();

    // Act & Assert - Non-matching requirements
    var req2 = new CapabilityRequirement { RequiresJsonMode = true };
    capabilities.Supports(req2).Should().BeFalse();

    // Act & Assert - Context size requirements
    var req3 = new CapabilityRequirement { MinContextTokens = 4096 };
    capabilities.Supports(req3).Should().BeTrue();

    var req4 = new CapabilityRequirement { MinContextTokens = 16384 };
    capabilities.Supports(req4).Should().BeFalse();
}

[Fact]
public void Should_Merge_Capabilities()
{
    // Arrange
    var cap1 = new ProviderCapabilities(
        supportsStreaming: true,
        supportsTools: false,
        maxContextTokens: 8192);

    var cap2 = new ProviderCapabilities(
        supportsStreaming: false,
        supportsTools: true,
        maxContextTokens: 16384);

    // Act
    var merged = cap1.Merge(cap2);

    // Assert
    merged.SupportsStreaming.Should().BeTrue(); // OR of both
    merged.SupportsToolCalls.Should().BeTrue(); // OR of both
    merged.MaxContextTokens.Should().Be(16384); // MAX of both
}
```

---

## Summary of Real Gaps

| Gap | FR | Type | Status |
|-----|----|-|--------|
| #1 | FR-020, FR-021 | ProviderDescriptor missing Priority, Enabled | ❌ Must implement |
| #2 | FR-025-028 | ProviderType wrong enum values | ❌ Must fix |
| #3 | FR-032-035 | ProviderCapabilities wrong/missing properties | ❌ Must fix |
| #4 | FR-036-037 | ProviderCapabilities missing methods | ❌ Must implement |
| #5 | Test Requirements | Missing Supports/Merge tests | ❌ Must add |

**All of these say MUST in the spec. None are optional.**

---

## Implementation Order

1. ✅ Create CapabilityRequirement record (new file)
2. ✅ Update ProviderCapabilities:
   - Rename SupportsTools → SupportsToolCalls
   - Rename MaxContextLength → MaxContextTokens
   - Add MaxOutputTokens property
   - Add SupportsJsonMode property
   - Add Supports() method
   - Add Merge() method
3. ✅ Update ProviderType enum (Ollama, Vllm, Mock)
4. ✅ Update ProviderDescriptor (Priority, Enabled properties)
5. ✅ Update ALL tests referencing old names
6. ✅ Add Should_Check_Supports() test
7. ✅ Add Should_Merge_Capabilities() test
8. ✅ Run full test suite
9. ✅ Verify all AC met

---

## Acceptance Criteria to Re-Verify

After fixes:
- AC-021: Priority property exists ✅
- AC-022: Enabled property exists ✅
- AC-025: Ollama value exists ✅
- AC-026: Vllm value exists ✅
- AC-027: Mock value exists ✅
- AC-028: Serialization to lowercase ✅
- AC-032: SupportsToolCalls property exists ✅
- AC-033: MaxContextTokens property exists ✅
- AC-034: MaxOutputTokens property exists ✅
- AC-035: SupportsJsonMode property exists ✅
- AC-036: Supports method works ✅
- AC-037: Merge method works ✅

---

**No more rationalizing. Implement what the spec says.**
