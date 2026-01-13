# PostStreamAsync Analysis - Should We Implement It?

**Date**: 2026-01-13
**Question**: Is the layered approach (PostStreamAsync) safer, more adaptable, or more manageable than the current direct HttpClient approach?

---

## Current Architecture (What Exists)

```
OllamaProvider.StreamChatAsync()
  ├─> HttpClient.PostAsync() [DIRECT]
  ├─> response.Content.ReadAsStreamAsync()
  ├─> OllamaStreamReader.ReadAsync(stream)
  └─> yield ResponseDelta
```

**Error Handling**: Duplicated in OllamaProvider (lines 138-155)
- HttpRequestException → OllamaServerException (5xx) or OllamaConnectionException
- TaskCanceledException → OllamaTimeoutException
- ❌ NO correlation ID
- ❌ NO structured logging

---

## Spec'd Architecture (What Should Exist)

```
OllamaProvider.StreamChatAsync()
  ├─> OllamaHttpClient.PostStreamAsync() [LAYERED]
  │     ├─> HttpClient.PostAsync()
  │     ├─> Error handling with correlation ID ✅
  │     ├─> Structured logging ✅
  │     └─> Returns Stream
  ├─> OllamaStreamReader.ReadAsync(stream)
  └─> yield ResponseDelta
```

**Error Handling**: Centralized in OllamaHttpClient (FR-093 to FR-099)
- ✅ Correlation ID included
- ✅ Structured logging
- ✅ Comprehensive exception wrapping
- ✅ Single source of truth

---

## Evidence of Duplication

### OllamaProvider Error Handling (Current - Lines 138-155)

```csharp
catch (HttpRequestException ex)
{
    if (ex.StatusCode.HasValue && (int)ex.StatusCode.Value >= 500)
    {
        throw new OllamaServerException(
            $"Ollama server returned error: {ex.Message}",
            ex,
            (int)ex.StatusCode.Value);
    }
    throw new OllamaConnectionException(
        $"Failed to connect to Ollama server at {this._config.BaseUrl}",
        ex);
}
catch (TaskCanceledException ex)
{
    if (cancellationToken.IsCancellationRequested) { throw; }
    throw new OllamaTimeoutException(
        $"Request to Ollama server timed out after {this._config.RequestTimeoutSeconds}s",
        ex);
}
```

**Issues**:
- ❌ No correlation ID
- ❌ No logging
- ❌ Simpler 5xx handling (doesn't distinguish HTTP errors)
- ❌ Duplicates logic that exists in OllamaHttpClient

### OllamaHttpClient Error Handling (Better - Lines 154-213)

```csharp
catch (TaskCanceledException ex)
{
    throw new OllamaTimeoutException(
        $"Request to {endpoint} timed out (CorrelationId: {this.CorrelationId})",
        ex);
}
catch (HttpRequestException ex)
{
    throw new OllamaConnectionException(
        $"Failed to connect to {endpoint} (CorrelationId: {this.CorrelationId})",
        ex);
}

// PLUS: Response status handling
try {
    response.EnsureSuccessStatusCode();
} catch (HttpRequestException ex) {
    if (statusCode >= 400 && statusCode < 500) {
        throw new OllamaRequestException(...);  // 4xx
    } else if (statusCode >= 500) {
        throw new OllamaServerException(...);   // 5xx
    }
}
```

**Benefits**:
- ✅ Correlation ID for tracing (FR-099)
- ✅ Structured logging (FR-040)
- ✅ Distinguishes 4xx vs 5xx
- ✅ Consistent with non-streaming PostAsync

---

## Downstream Impact Analysis

### Task 005 (Parent) Expectations

**Test Code from task-005-ollama-provider-adapter.md**:
```csharp
_mockClient
    .Setup(c => c.PostStreamAsync(
        "/api/chat",
        It.IsAny<OllamaRequest>(),
        It.IsAny<CancellationToken>()))
    .Returns(ToAsyncEnumerable(chunks));
```

**Evidence**: Parent task EXPECTS PostStreamAsync to exist for mocking in tests.

**Current Problem**:
- Tests cannot mock OllamaHttpClient.PostStreamAsync (doesn't exist)
- Must mock HttpClient instead (less clean)
- Harder to write unit tests for OllamaProvider

---

## Benefits Analysis

### Option A: Implement PostStreamAsync (Layered Approach)

#### Safety ✅ BETTER
1. **Centralized error handling**: All HTTP errors handled in one place
2. **Consistent behavior**: Streaming uses same error handling as non-streaming
3. **Correlation tracking**: All requests get correlation IDs automatically
4. **Observability**: All requests get logged automatically

#### Adaptability ✅ BETTER
1. **Reusable**: Other providers can use PostStreamAsync
2. **Mockable**: Easy to mock for testing
3. **Extensible**: Can add retry logic, circuit breakers in one place
4. **Configurable**: Request options centralized

#### Manageability ✅ BETTER
1. **Single source of truth**: HTTP logic in one class
2. **Easier debugging**: Correlation IDs trace through logs
3. **Consistent**: Same patterns for streaming and non-streaming
4. **DRY**: No duplicated error handling

### Option B: Current Implementation (Direct HttpClient)

#### Safety ❌ WORSE
1. **Duplicated error handling**: Same logic in Provider and HttpClient
2. **Inconsistent**: Provider's error handling is simpler (no correlation ID)
3. **Missing observability**: Provider calls don't get logged

#### Adaptability ❌ WORSE
1. **Not reusable**: Each provider reimplements streaming
2. **Hard to mock**: Must mock HttpClient (verbose)
3. **Scattered logic**: HTTP concerns in multiple places

#### Manageability ❌ WORSE
1. **Code duplication**: Error handling repeated
2. **Harder debugging**: No correlation IDs on streaming requests
3. **Inconsistent**: Different patterns for streaming vs non-streaming

---

## Consequences of Not Following Spec

### Immediate Consequences

1. **Audit Failure**: FR-062 through FR-067 not met ❌
2. **Test Issues**: Parent task-005 tests expect PostStreamAsync to exist
3. **Code Duplication**: OllamaProvider duplicates error handling (lines 138-155)
4. **Missing Observability**: Streaming requests not logged with correlation IDs

### Future Consequences

1. **Harder to add other providers**: Each must reimplement streaming
2. **Inconsistent error handling**: Different exceptions for same errors
3. **Difficult to add middleware**: Retry logic, circuit breakers must be per-provider
4. **Testing complexity**: Cannot mock clean interface boundaries

---

## Recommendation: Option A - Implement PostStreamAsync

### Justification

1. **Eliminates duplication**: Remove 20+ lines from OllamaProvider
2. **Improves observability**: All requests logged with correlation IDs
3. **Follows spec**: Meets FR-062 through FR-067
4. **Enables testing**: Parent task tests can mock cleanly
5. **Better architecture**: Proper separation of concerns

### Implementation Cost

**Time**: ~20-30 minutes
**Complexity**: LOW - mostly moving code from Provider to HttpClient
**Risk**: LOW - can test thoroughly before migrating Provider

### Migration Steps

1. **Add PostStreamAsync to OllamaHttpClient** (15 min)
   ```csharp
   public async Task<Stream> PostStreamAsync(
       string endpoint,
       object request,
       CancellationToken cancellationToken = default)
   {
       // Use existing error handling from PostAsync
       // Return Stream without disposing (FR-064)
   }
   ```

2. **Update OllamaProvider** (5 min)
   ```csharp
   // Before: 20 lines of error handling + HttpClient.PostAsync
   var stream = await _httpClient.PostStreamAsync(
       "/api/chat",
       streamingRequest,
       cancellationToken);

   // Remove duplicated error handling (lines 138-155)
   ```

3. **Add tests** (10 min)
   - Test PostStreamAsync returns Stream
   - Test error handling with correlation ID
   - Test cancellation support

4. **Run full test suite** (2 min)
   - Verify 1377/1377 tests still pass
   - Verify integration tests work

---

## Alternative: Option B - Document Deviation

**Only valid if**:
- User explicitly approves deviation
- Spec updated to remove FR-062 through FR-067
- Duplication documented as "accepted technical debt"
- Future tasks updated to not expect PostStreamAsync

**Not recommended** because:
- Doesn't solve duplication problem
- Doesn't improve observability
- Makes future providers harder to implement
- Violates clean architecture principles

---

## Decision Required

**Question**: Should we implement PostStreamAsync per spec (Option A)?

**My strong recommendation**: YES
- Benefits clearly outweigh costs
- Eliminates code duplication
- Improves observability
- Follows spec and enables future work
- Low implementation cost (~30 min)

**User approval requested**: Implement PostStreamAsync and migrate OllamaProvider to use it?
