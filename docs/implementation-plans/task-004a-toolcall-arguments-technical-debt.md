# Task 004a - ToolCall.Arguments Technical Debt Fix

## Overview

**Issue**: ToolCall.Arguments was implemented as `string` but spec FR-004a-45 requires `JsonElement` type.

**Branch**: `feature/task-004a-fix-technical-debt`

**Parent Task**: task-004a-define-message-tool-call-types.md

## Gap Analysis

### Current Implementation
```csharp
public sealed record ToolCall(string Id, string Name, string Arguments)
{
    public string Arguments { get; init; } = ValidateArguments(Arguments).Trim();

    // Methods parse Arguments string multiple times
    public bool TryGetArgument<T>(string key, out T? value)
    {
        using var doc = JsonDocument.Parse(this.Arguments);
        // ... parsing happens here
    }
}
```

### Target Implementation (FR-004a-45)
```csharp
public sealed record ToolCall(string Id, string Name, JsonElement Arguments)
{
    public JsonElement Arguments { get; init; } = ValidateArguments(Arguments);

    // Methods work directly with JsonElement (no re-parsing)
    public bool TryGetArgument<T>(string key, out T? value)
    {
        if (this.Arguments.TryGetProperty(key, out var element))
        {
            // ... direct access, no parsing
        }
    }
}
```

### Benefits of JsonElement
1. **Type Safety**: Compile-time guarantee it's valid JSON
2. **Performance**: No re-parsing on every method call
3. **API Correctness**: Matches spec exactly
4. **Immutability**: JsonElement is inherently immutable

### Breaking Changes
This is a **breaking change** affecting:
- All code that creates ToolCall instances
- All code that reads ToolCall.Arguments
- All tests that use ToolCall
- Serialization/deserialization logic

## Files Requiring Changes

### Production Code (TDD: Write tests first, then implement)

1. **src/Acode.Domain/Models/Inference/ToolCall.cs**
   - Change Arguments from `string` to `JsonElement`
   - Update ValidateArguments to accept/return JsonElement
   - Update TryGetArgument<T> to work with JsonElement directly
   - Update GetArgumentsAs<T> to work with JsonElement directly
   - Update constructor/validation

2. **src/Acode.Domain/Models/Inference/ChatMessage.cs**
   - Update factory methods that create ToolCall instances (if any)

3. **src/Acode.Domain/Models/Inference/Serialization/MessageJsonContext.cs**
   - Verify JsonElement serialization support (should work automatically)

### Test Files (RED phase - write these first)

4. **tests/Acode.Domain.Tests/Models/Inference/ToolCallTests.cs**
   - Update all test data from strings to JsonElement
   - Add helper method to create JsonElement from string
   - Update assertions to work with JsonElement
   - Add new tests for JsonElement-specific behavior

5. **tests/Acode.Integration.Tests/Models/Messages/SerializationCompatibilityTests.cs**
   - Update ToolCall creation to use JsonElement
   - Verify serialization still produces correct JSON strings

6. **tests/Acode.Domain.Tests/Models/Inference/ConversationHistoryTests.cs**
   - Update if it creates ToolCall instances

### Other Potentially Affected Files

7. **src/Acode.Infrastructure/Tools/ToolSchemaRegistry.cs**
   - Check if it creates ToolCall instances

8. **tests/Acode.Infrastructure.Tests/Ollama/ToolCall/ToolCallParserTests.cs**
   - Update if it creates ToolCall instances

## Implementation Checklist

### Phase 1: Preparation (Before Code Changes)

- [ ] **Step 1.1**: Switch to main branch and pull latest
  ```bash
  git checkout main
  git pull origin main
  ```

- [ ] **Step 1.2**: Create new feature branch
  ```bash
  git checkout -b feature/task-004a-fix-technical-debt
  ```

- [ ] **Step 1.3**: Verify current tests pass (baseline)
  ```bash
  dotnet test --no-build
  ```

- [ ] **Step 1.4**: Read all affected files to understand current usage
  - Read ToolCall.cs
  - Read ToolCallTests.cs
  - Read SerializationCompatibilityTests.cs
  - Grep for all `new ToolCall(` patterns

### Phase 2: RED - Update Tests First (TDD)

- [ ] **Step 2.1**: Create test helper in ToolCallTests.cs
  ```csharp
  private static JsonElement CreateJsonElement(string json)
  {
      var doc = JsonDocument.Parse(json);
      return doc.RootElement.Clone();
  }
  ```

- [ ] **Step 2.2**: Update ToolCallTests.cs - Basic Property Tests
  - Replace all `new ToolCall(..., "{...}")` with `new ToolCall(..., CreateJsonElement("{...}"))`
  - Expected: Tests fail with compilation errors (string vs JsonElement)
  - Update approximately 15-20 test methods

- [ ] **Step 2.3**: Update ToolCallTests.cs - TryGetArgument Tests
  - Update test data to use JsonElement
  - Verify test logic still checks correct behavior
  - Expected: Compilation errors

- [ ] **Step 2.4**: Update ToolCallTests.cs - GetArgumentsAs Tests
  - Update test data to use JsonElement
  - Expected: Compilation errors

- [ ] **Step 2.5**: Update ToolCallTests.cs - Validation Tests
  - Update invalid JSON tests to pass JsonElement
  - Expected: Compilation errors

- [ ] **Step 2.6**: Update SerializationCompatibilityTests.cs
  - Find all ToolCall creation (search for `new ToolCall(`)
  - Update to use JsonElement via helper method
  - Expected: Compilation errors

- [ ] **Step 2.7**: Update ConversationHistoryTests.cs (if needed)
  - Check if file creates ToolCall instances
  - Update if necessary
  - Expected: Compilation errors if affected

- [ ] **Step 2.8**: Verify tests don't compile (RED phase confirmed)
  ```bash
  dotnet build
  # Expected: Compilation errors about string/JsonElement mismatch
  ```

### Phase 3: GREEN - Implement Changes

- [ ] **Step 3.1**: Update ToolCall.cs - Change Arguments Property
  ```csharp
  // BEFORE:
  public sealed record ToolCall(string Id, string Name, string Arguments)

  // AFTER:
  public sealed record ToolCall(string Id, string Name, JsonElement Arguments)
  ```

- [ ] **Step 3.2**: Update ToolCall.cs - Change Arguments Property Declaration
  ```csharp
  // BEFORE:
  [JsonPropertyName("arguments")]
  public string Arguments { get; init; } = ValidateArguments(Arguments).Trim();

  // AFTER:
  [JsonPropertyName("arguments")]
  public JsonElement Arguments { get; init; } = ValidateArguments(Arguments);
  ```

- [ ] **Step 3.3**: Update ToolCall.cs - Update ValidateArguments Method
  ```csharp
  // BEFORE:
  private static string ValidateArguments(string arguments)
  {
      if (string.IsNullOrWhiteSpace(arguments))
      {
          throw new ArgumentException("ToolCall Arguments must be non-empty.", nameof(Arguments));
      }

      try
      {
          using var doc = JsonDocument.Parse(arguments);
          _ = doc.RootElement;
      }
      catch (JsonException ex)
      {
          throw new ArgumentException("ToolCall Arguments must be valid JSON.", nameof(Arguments), ex);
      }

      return arguments;
  }

  // AFTER:
  private static JsonElement ValidateArguments(JsonElement arguments)
  {
      // FR-004a-46: Arguments MUST be valid JSON object
      if (arguments.ValueKind != JsonValueKind.Object)
      {
          throw new ArgumentException("ToolCall Arguments must be a JSON object.", nameof(Arguments));
      }

      return arguments;
  }
  ```

- [ ] **Step 3.4**: Update ToolCall.cs - Update TryGetArgument<T> Method
  ```csharp
  // BEFORE:
  public bool TryGetArgument<T>(string key, out T? value)
  {
      try
      {
          using var doc = JsonDocument.Parse(this.Arguments);
          if (doc.RootElement.TryGetProperty(key, out var element))
          {
              value = JsonSerializer.Deserialize<T>(element.GetRawText());
              return value is not null;
          }

          value = default;
          return false;
      }
      catch
      {
          value = default;
          return false;
      }
  }

  // AFTER:
  public bool TryGetArgument<T>(string key, out T? value)
  {
      try
      {
          if (this.Arguments.TryGetProperty(key, out var element))
          {
              value = JsonSerializer.Deserialize<T>(element.GetRawText());
              return value is not null;
          }

          value = default;
          return false;
      }
      catch
      {
          value = default;
          return false;
      }
  }
  ```

- [ ] **Step 3.5**: Update ToolCall.cs - Update GetArgumentsAs<T> Method
  ```csharp
  // BEFORE:
  public T? GetArgumentsAs<T>()
      where T : class
  {
      try
      {
          var options = new JsonSerializerOptions
          {
              PropertyNameCaseInsensitive = true,
          };
          return JsonSerializer.Deserialize<T>(this.Arguments, options);
      }
      catch
      {
          return null;
      }
  }

  // AFTER:
  public T? GetArgumentsAs<T>()
      where T : class
  {
      try
      {
          var options = new JsonSerializerOptions
          {
              PropertyNameCaseInsensitive = true,
          };
          return JsonSerializer.Deserialize<T>(this.Arguments.GetRawText(), options);
      }
      catch
      {
          return null;
      }
  }
  ```

- [ ] **Step 3.6**: Build and verify compilation succeeds
  ```bash
  dotnet build --no-incremental
  # Expected: Build succeeds, 0 errors
  ```

- [ ] **Step 3.7**: Run ToolCall unit tests
  ```bash
  dotnet test tests/Acode.Domain.Tests --filter "FullyQualifiedName~ToolCallTests" --no-build
  # Expected: All tests pass
  ```

- [ ] **Step 3.8**: Run integration tests
  ```bash
  dotnet test tests/Acode.Integration.Tests --filter "FullyQualifiedName~SerializationCompatibilityTests" --no-build
  # Expected: All tests pass
  ```

### Phase 4: REFACTOR - Additional Improvements

- [ ] **Step 4.1**: Add JsonElement convenience tests
  - Test that Arguments.ValueKind == JsonValueKind.Object
  - Test that Arguments can be accessed without parsing
  - Test edge cases (empty object, nested objects)

- [ ] **Step 4.2**: Update code comments/documentation
  - Update XML comments to reflect JsonElement type
  - Update remarks to mention direct access benefits

- [ ] **Step 4.3**: Check other Infrastructure code
  - Search for any ToolCall creation in Infrastructure layer
  - Update ToolSchemaRegistry if needed
  - Update ToolCallParser if needed

- [ ] **Step 4.4**: Run full test suite
  ```bash
  dotnet test --no-build
  # Expected: All previously passing tests still pass
  ```

### Phase 5: Commit and Push

- [ ] **Step 5.1**: Review all changes
  ```bash
  git status
  git diff
  ```

- [ ] **Step 5.2**: Commit changes
  ```bash
  git add .
  git commit -m "fix(task-004a): Change ToolCall.Arguments from string to JsonElement

Implements FR-004a-45 requirement for JsonElement type instead of string.

Benefits:
- Type safety: Compile-time guarantee of valid JSON
- Performance: No re-parsing on method calls
- API correctness: Matches spec exactly
- Immutability: JsonElement is inherently immutable

Breaking Changes:
- ToolCall constructor now requires JsonElement instead of string
- All ToolCall creation sites must use JsonDocument.Parse().RootElement
- Helper methods added to tests for easier JsonElement creation

Updated files:
- src/Acode.Domain/Models/Inference/ToolCall.cs
- tests/Acode.Domain.Tests/Models/Inference/ToolCallTests.cs
- tests/Acode.Integration.Tests/Models/Messages/SerializationCompatibilityTests.cs

All tests passing.

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"
  ```

- [ ] **Step 5.3**: Push to remote
  ```bash
  git push origin feature/task-004a-fix-technical-debt
  ```

- [ ] **Step 5.4**: Create PR
  ```bash
  gh pr create --title "fix(task-004a): Change ToolCall.Arguments from string to JsonElement" --body "$(cat <<'EOF'
## Summary

Fixes technical debt from task 004a by implementing FR-004a-45 correctly: ToolCall.Arguments must be JsonElement, not string.

## Changes

### Breaking Changes
- **ToolCall.Arguments** type changed from `string` to `JsonElement`
- All ToolCall creation sites must now pass `JsonElement` via `JsonDocument.Parse(jsonString).RootElement`

### Benefits
1. **Type Safety**: Compile-time guarantee that Arguments is valid JSON
2. **Performance**: No re-parsing on every method call (TryGetArgument, GetArgumentsAs)
3. **API Correctness**: Matches spec FR-004a-45 exactly
4. **Immutability**: JsonElement is inherently immutable

### Implementation Details
- Updated `ValidateArguments` to work with JsonElement directly
- Simplified `TryGetArgument<T>` - no JsonDocument.Parse needed
- Simplified `GetArgumentsAs<T>` - uses GetRawText() only once
- Added test helper `CreateJsonElement(string)` for easier test data creation

## Testing

- ✅ All ToolCall unit tests pass (15+ tests)
- ✅ All SerializationCompatibility tests pass (8 tests)
- ✅ Full test suite passes

## Migration Guide

**Before:**
```csharp
var toolCall = new ToolCall("call_1", "my_tool", "{\"key\":\"value\"}");
```

**After:**
```csharp
var arguments = JsonDocument.Parse("{\"key\":\"value\"}").RootElement;
var toolCall = new ToolCall("call_1", "my_tool", arguments);
```

🤖 Generated with [Claude Code](https://claude.com/claude-code)
EOF
)"
  ```

### Phase 6: Verification

- [ ] **Step 6.1**: Verify PR created successfully
- [ ] **Step 6.2**: Check CI pipeline (if exists)
- [ ] **Step 6.3**: Mark task complete in this checklist
- [ ] **Step 6.4**: Update task-004a-completion-checklist.md to reflect technical debt resolved

## Success Criteria

✅ All tests pass (no regressions)
✅ ToolCall.Arguments is JsonElement type (FR-004a-45)
✅ No performance degradation (should be faster)
✅ All affected files updated
✅ Breaking changes documented
✅ PR created and ready for review

## Notes

- This is a **breaking change** - any code using ToolCall will need updates
- JsonElement is more efficient (avoids re-parsing)
- JsonElement provides compile-time type safety
- This fully implements FR-004a-45 as specified

## Estimated Impact

- **Files Changed**: 3-5 files (ToolCall.cs + tests)
- **Lines Changed**: ~50-100 lines
- **Test Updates**: ~20-30 test methods
- **Breaking Change**: YES - all ToolCall creation sites affected
